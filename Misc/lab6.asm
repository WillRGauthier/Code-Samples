# Title: 	Lab6 assignment - Clock Counter
# Description:	this is a template file for lab6

.kdata			            # kernel data
semicolon: .asciiz ":"
new_line: .asciiz "\n"
zero: .asciiz "0"

.data
max_days: .word 1#20
max_hours: .word 1#23
max_mins: .word 1#59
max_secs: .word 1#59

.text
.globl main
main:
	#Code to enable the keyboard interrupt
	lui $t0, 0xFFFF			#$t0 = 0xFFFF0000;
	ori $a0, $0, 2			#enable keyboard interrupt
	sw $a0, 0($t0)	
	
	mfc0 $a0, $12			# read from the status register
	ori $a0, 0xff11			# enable all interrupts
	mtc0 $a0, $12			# write back to the status register
	
	#Code to initialize clock
	li $v0, 30			# service code 30 saves low order time bits in $a0 and high order bits in $a1
	syscall
	move $s0, $a0			# registers $s0 and $s1 store clock start time in milliseconds
	move $s1, $a1
	
	move $s2, $0			# $s2 stores days
	move $s3, $0			# $s3 stores hours
	move $s4, $0			# $s4 stores minutes
	move $s5, $0			# $s5 stores seconds
	
	update_clock:
	li $v0, 30
	syscall
	
	# subtract current time from start time in $s0 and $s1
	sub64: #a1a0 - s1s0 = a1a0 + ~s1~s0 + 1
		nor $t0, $s0, $0 # $t0 = ~$s0
		nor $t1, $s1, $0 # $t1 = ~$s1
		jal add64
		# add 1
		move $a1, $0
		move $a0, $0
		move $t1, $0
		addi $t0, $0, 1

	# divide $t2 and $t3 by 1000 to get total seconds
	addi $t4, $0, 1000
	jal div64
	# divide $t2 and $t3 to get total minutes, set remainder to $s5
	lw $t7, max_secs
	add $t4, $0, $t7
	addi $t4, $t4, 1
	jal div64
	move $s5, $t6
	# divide $t2 and $t3 to get total hours, set remainder to $s4
	lw $t7, max_mins
	add $t4, $0, $t7
	addi $t4, $t4, 1
	jal div64
	move $s4, $t6
	# divide $t2 and $t3 to get total days, set remainder to $s3
	lw $t7, max_hours
	add $t4, $0, $t7
	addi $t4, $t4, 1
	jal div64
	move $s3, $t6
	move $s2, $t2
	
	lw $t7, max_days
	tlt $t7, $s2
	j update_clock
	
	li $v0, 10				# exit,if it ever comes here
	syscall
	
	
add64: # stores high in $t3 and low in $t2
	addu $t2, $a0, $t0
	nor $t3, $t0, $0 # 2^32 - 1 = $t0 + ~$t0; $a0 + $t0 > 2^32 - 1 = $a0 + $t0 > $t0 + ~$t0 = $a0 > ~t0
	sltu $t3, $t3, $a0
	addu $t3, $t3, $a1
	addu $t3, $t3, $t1
	jr $ra
	
div64: # keeps high in $t3 and low in $t2 and stores remainder in $t6; uses $t4 as divisor
	div $t3, $t4
	mflo $t3
	mfhi $t5
	# move remainder to most significant bit
	beqz $t5, shifted # do nothing if no remainder
	msb_zero:
		andi $t6, $t5, 0x80000000
		bnez $t6, shifted
		sll $t5, $t5, 1
		j msb_zero
	shifted:
	addu $t2, $t2, $t5
	div $t2, $t4
	mflo $t2
	mfhi $t6
	jr $ra
	

.ktext 0x80000180		# kernel code starts here
	move $s6, $v0				# We need to use these registers
	move $s7, $a0				# not using the stack because the interrupt might be triggered by a memory reference 
	move $a3, $ra				        # using a bad value of the stack pointer
					        
	mfc0 $k0, $13			# Cause register
	srl $a0, $k0, 2			# Extract ExcCode Field
	andi $a0, $a0, 0x1F
	
	beqz $a0, input # code 0 is I/O
	beq $a0, 13, reset_clock # code 13 is trap
	
kdone:
	mtc0 $0, $13			# Clear Cause register
	mfc0 $k0, $12			# Set Status register
	andi $k0, 0xfffd		# clear EXL bit
	ori  $k0, 0x11			# Interrupts enabled
	mtc0 $k0, $12			# write back to status

	move $v0, $s6			# Restore other registers
	move $a0, $s7
	move $ra, $a3
    	
    	eret				# return to EPC


input:
	lui $v0, 0xFFFF			# $t0 = 0xFFFF0000;
	lw $a0, 4($v0)			# get the input key	
	beq $a0, 0x31, display
	beq $a0, 0x32, kexit
	j kdone
	
display:	
	#code to display the current time
	move $k1, $s2
	jal print0 
	li $v0, 1
	move $a0, $s2
	syscall
	li $v0, 4
	la $a0, semicolon
	syscall
	move $k1, $s3
	jal print0 
	li $v0, 1
	move $a0, $s3
	syscall
	li $v0, 4
	la $a0, semicolon
	syscall
	move $k1, $s4
	jal print0 
	li $v0, 1
	move $a0, $s4
	syscall
	li $v0, 4
	la $a0, semicolon
	syscall
	move $k1, $s5
	jal print0 
	li $v0, 1
	move $a0, $s5
	syscall
	li $v0, 4
	la $a0, new_line
	syscall
	
	j kdone
	
print0: bge $k1, 10, good
	li $v0, 4
	la $a0, zero
	syscall
	good:
		jr $ra
	
reset_clock:
	li $v0, 30			# service code 30 saves low order time bits in $a0 and high order bits in $a1
	syscall
	
	move $s0, $a0			# registers $s0 and $s1 store clock start time in milliseconds
	move $s1, $a1
	
	move $s2, $0			# $s2 stores days
	move $s3, $0			# $s3 stores hours
	move $s4, $0			# $s4 stores minutes
	move $s5, $0			# $s5 stores seconds
	
	j kdone
	
kexit:
	li $v0, 10
	syscall
