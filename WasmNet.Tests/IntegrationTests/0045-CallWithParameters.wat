;; invoke: getAnswerPlus (i32:1)
;; expect: (i32:43)

(module
    (func $getAnswer (result i32)
        i32.const 42)
    (func $add (param $x i32) (param $y i32) (result i32)
        local.get $x
        local.get $y
        i32.add)
    (func (export "getAnswerPlus") (param $x i32) (result i32)
        call $getAnswer
        local.get $x
        call $add))
